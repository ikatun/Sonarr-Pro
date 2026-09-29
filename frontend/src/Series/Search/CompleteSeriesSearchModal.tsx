import React from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds, scrollDirections, sizes } from 'Helpers/Props';
import InteractiveSearch from 'InteractiveSearch/InteractiveSearch';
import translate from 'Utilities/String/translate';

interface CompleteSeriesSearchModalProps {
  isOpen: boolean;
  seriesId: number;
  onModalClose(): void;
}

function CompleteSeriesSearchModal({
  isOpen,
  seriesId,
  onModalClose,
}: CompleteSeriesSearchModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_EXTRA_LARGE}
      closeOnBackgroundClick={false}
      onModalClose={onModalClose}
    >
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>{translate('SearchCompleteSeries')}</ModalHeader>
        <ModalBody scrollDirection={scrollDirections.BOTH}>
          <Alert kind={kinds.INFO}>
            {translate('SearchCompleteSeriesHelpText')}
          </Alert>
          <InteractiveSearch
            type="series"
            searchPayload={{ seriesId, completeSeries: true }}
          />
        </ModalBody>
        <ModalFooter>
          <Button onPress={onModalClose}>{translate('Close')}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default CompleteSeriesSearchModal;
